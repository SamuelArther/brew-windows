class LmSensors < Formula
  desc "Tools for monitoring the temperatures and fans"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "sensors", "sensors"
    windows_exe "libexec/wintools.exe", "sensors-detect", "sensors-detect"
  end

  test do
    assert_match "Usage", shell_output("#{bin}/sensors --help")
    assert_match "sensors-detect version", shell_output("#{bin}/sensors-detect")
    assert_match "sensors version", shell_output("#{bin}/sensors -v")
  end
end
