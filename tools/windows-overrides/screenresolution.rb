class Screenresolution < Formula
  desc "Get, set, and list display resolution"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "screenresolution", "screenresolution"
  end

  test do
    assert_match "screenresolution get", shell_output("#{bin}/screenresolution --help")
    assert_match(/version \d+\.\d+/, shell_output("#{bin}/screenresolution -version"))
    assert_match "bad mode", shell_output("#{bin}/screenresolution set 12x 2>&1", 2)
  end
end
