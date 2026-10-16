class WhisperkitCli < Formula
  desc "On-device speech recognition with Whisper models (whisper.cpp on Windows)"
  homepage "https://github.com/ggml-org/whisper.cpp"
  url "https://github.com/ggml-org/whisper.cpp/releases/download/b5454/whisper-bin-x64.zip"
  version "1.9.5"
  sha256 "6ba69e3482d7826214f90a6a9c84ca07782aec1e1d0c6a7c30c994fd5d816ccb"
  license "MIT"

  # WhisperKit runs Whisper through Core ML and only builds for Apple platforms. whisper.cpp is
  # the standard Whisper runtime on Windows (official ggml-org CPU build for the v1.9.5 release).
  # It uses ggml model files and whisper.cpp's options instead of WhisperKit's:
  #   whisperkit-cli -m ggml-base.en.bin -f audio.wav

  livecheck do
    url "https://github.com/ggml-org/whisper.cpp"
    strategy :github_latest
  end

  def caveats
    <<~EOS
      whisperkit-cli is whisper.cpp's whisper-cli. Download a ggml model first, e.g.
        https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.en.bin
      then: whisperkit-cli -m ggml-base.en.bin -f audio.wav
    EOS
  end

  def install
    install_tree
    %w[whisperkit-cli whisper-cli].each { |cmd| windows_exe "libexec/whisper-cli.exe", cmd }
    windows_exe "libexec/whisper-server.exe", "whisper-server"
    windows_exe "libexec/whisper-stream.exe", "whisper-stream"
    windows_exe "libexec/whisper-bench.exe", "whisper-bench"
    windows_exe "libexec/whisper-quantize.exe", "whisper-quantize"
  end

  test do
    output = shell_output("#{bin}/whisperkit-cli --help 2>&1")
    assert_match "usage", output
    assert_match "--model", output
  end
end
