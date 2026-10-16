class Yap < Formula
  desc "On-device audio transcription using whisper.cpp"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "yap", "yap"
  end

  test do
    assert_match(/\A\S*\s?v?\d+(\.\d+)+/, shell_output("#{bin}/yap --version").strip)
    assert_match "transcribe", shell_output("#{bin}/yap help")
    assert_match "transcribe", shell_output("#{bin}/yap --help")
  end
end
