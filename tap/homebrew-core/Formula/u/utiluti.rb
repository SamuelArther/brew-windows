class Utiluti < Formula
  desc "Command-line tool to work with default apps"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "utiluti", "utiluti"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/utiluti --version"))
    notepad = File.join(ENV["SystemRoot"], "System32", "notepad.exe")
    assert_match(/\A\d+(\.\d+)+/, shell_output("#{bin}/utiluti app version #{notepad}").strip)
    assert_match "notepad", shell_output("#{bin}/utiluti app identifier #{notepad}")
  end
end
