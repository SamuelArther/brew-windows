class Utiluti < Formula
  desc "Command-line tool to work with default apps"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "utiluti", "utiluti"
  end

  test do
    assert_match(/\d+\.\d+/, shell_output("#{bin}/utiluti --version"))
    notepad = File.join(ENV["SystemRoot"], "System32", "notepad.exe")
    assert_match(/\A\d+(\.\d+)+/, shell_output("#{bin}/utiluti app version #{notepad}").strip)
    assert_match(/notepad/i, shell_output("#{bin}/utiluti app identifier #{notepad}"))
  end
end
