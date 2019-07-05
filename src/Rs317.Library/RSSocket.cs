
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;

public sealed class RSSocket : IRunnable
{
	private NetworkStream inputStream;

	private NetworkStream outputStream;

	private TcpClient socket;

	private bool closed;

	private RSApplet rsApplet;

	private byte[] buffer;

	private int writeIndex;

	private int buffIndex;

	private bool isWriter;
	private bool hasIOError;

	//TODO: Add exception documentation
	/// <summary>
	/// 
	/// </summary>
	/// <exception cref=""></exception>
	/// <returns></returns>
	public RSSocket(RSApplet RSApplet_, TcpClient socket1)
	{
		closed = false;
		isWriter = false;
		hasIOError = false;
		rsApplet = RSApplet_;
		socket = socket1;
		socket.SendTimeout = 30000;
		socket.NoDelay = true;
		inputStream = socket.GetStream();
		outputStream = socket.GetStream();
	}

	//TODO: Add exception documentation
	/// <summary>
	/// 
	/// </summary>
	/// <exception cref=""></exception>
	/// <returns></returns>
	public int available()
	{
		if(closed)
			return 0;
		else
			return socket.Available;
	}

	public void close()
	{
		closed = true;
		try
		{
			if(inputStream != null)
				inputStream.Close();
			if(outputStream != null)
				outputStream.Close();
			if(socket != null)
				socket.Close();
		}
		catch(Exception _ex)
		{
			throw new InvalidOperationException($"Error closing stream. Error: {_ex.Message}", _ex);
		}
		isWriter = false;

		//Prevent runnable thread from hanging.
		lock (this)
		{
			Monitor.PulseAll(this);
		}

		buffer = null;
	}

	public void printDebug()
	{
		Console.WriteLine("dummy:" + closed);
		Console.WriteLine("tcycl:" + writeIndex);
		Console.WriteLine("tnum:" + buffIndex);
		Console.WriteLine("writer:" + isWriter);
		Console.WriteLine("ioerror:" + hasIOError);
		try
		{
			Console.WriteLine("available:" + available());
		}
		catch(IOException _ex)
		{
		}
	}

	//TODO: Add exception documentation
	/// <summary>
	/// 
	/// </summary>
	/// <exception cref=""></exception>
	/// <returns></returns>
	public int read()
	{
		if(closed)
			return 0;
		else
			return inputStream.ReadByte();
	}

	//TODO: Add exception documentation
	/// <summary>
	/// 
	/// </summary>
	/// <exception cref=""></exception>
	/// <returns></returns>
	public void read(byte[] abyte0, int j)
	{
		int i = 0;// was parameter
		if(closed)
			return;
		int k;
		for(; j > 0; j -= k)
		{
			k = inputStream.Read(abyte0, i, j);
			if(k <= 0)
				throw new IOException("EOF");
			i += k;
		}

	}

	public void run()
	{
		while(isWriter)
		{
			int i;
			int j;
			lock(this)
			{
				if(buffIndex == writeIndex)
					try
					{
						Monitor.Wait(this);
					}
					catch(Exception _ex)
					{
					}
				if(!isWriter)
					return;
				j = writeIndex;
				if(buffIndex >= writeIndex)
					i = buffIndex - writeIndex;
				else
					i = 5000 - writeIndex;
			}
			if(i > 0)
			{
				try
				{
					outputStream.Write(buffer, j, i);
				}
				catch(IOException _ex)
				{
					hasIOError = true;
				}
				writeIndex = (writeIndex + i) % 5000;
				try
				{
					if(buffIndex == writeIndex)
						outputStream.Flush();
				}
				catch(IOException _ex)
				{
					hasIOError = true;
				}
			}
		}
	}

	//TODO: Add exception documentation
	/// <summary>
	/// 
	/// </summary>
	/// <exception cref=""></exception>
	/// <returns></returns>
	public void write(int i, byte[] abyte0)
	{
		if(closed)
			return;
		if(hasIOError)
		{
			hasIOError = false;
			throw new IOException("Error in writer thread");
		}
		if(buffer == null)
			buffer = new byte[5000];
		lock(this)
		{
			for(int l = 0; l < i; l++)
			{
				buffer[buffIndex] = abyte0[l];
				buffIndex = (buffIndex + 1) % 5000;
				if(buffIndex == (writeIndex + 4900) % 5000)
					throw new IOException("buffer overflow");
			}

			if(!isWriter)
			{
				isWriter = true;
				rsApplet.startRunnable(this, 3);
			}
			Monitor.PulseAll(this);
		}
	}
}
