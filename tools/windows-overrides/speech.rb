class Speech < Formula
  desc "On-device speech toolkit: ASR (whisper.cpp), TTS, VAD and an audio API server"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "speech", "speech"
    windows_exe "libexec/wintools.exe", "speech-server", "speech-server"
  end

  def caveats
    <<~EOS
      Speech recognition uses whisper.cpp and a ggml Whisper model:
        brew install whisperkit-cli
        set SPEECH_MODEL=<path to ggml-base.en.bin> (or pass --model)
      Text-to-speech uses the voices installed in Windows.
    EOS
  end

  test do
    assert_match "--model", shell_output("#{bin}/speech voice-chat --help")
    output = shell_output("#{bin}/speech transcribe /nonexistent.wav 2>&1", 1)
    assert_match "Error", output
    # 1.5 s of 8 kHz 8-bit PCM: near-silence, 0.5 s of a loud square wave, near-silence
    header = "RIFF\x24\x2F\x00\x00WAVEfmt \x10\x00\x00\x00\x01\x00\x01\x00" \
             "\x40\x1F\x00\x00\x40\x1F\x00\x00\x01\x00\x08\x00data\x00\x2F\x00\x00"
    (testpath/"tone.wav").write(header + ("\x7F" * 4000) + ("\x00\x7F" * 2000) + ("\x7F" * 4032))
    assert_match "1 segment(s)", shell_output("#{bin}/speech vad #{testpath}/tone.wav")
    port = free_port
    pid = spawn bin/"speech-server", "--host", "127.0.0.1", "--port", port.to_s
    begin
      sleep 3
      assert_match "ok", shell_output("curl -sf --max-time 5 http://127.0.0.1:#{port}/health")
    ensure
      Process.kill("TERM", pid)
    end
  end
end
