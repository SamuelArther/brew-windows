class Zzz < Formula
  desc "Command-line tool to put the PC to sleep"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "Zzz", "Zzz"
  end

  test do
    assert_match "Puts the PC to sleep", shell_output("#{bin}/Zzz -h")
    assert_match "unknown option", shell_output("#{bin}/Zzz --bogus 2>&1", 2)
  end
end
