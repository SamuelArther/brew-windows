class Locateme < Formula
  desc "Find your location using the Windows Location service"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "LocateMe", "LocateMe"
  end

  test do
    system bin/"LocateMe", "-h"
    assert_match "{LAT}", shell_output("#{bin}/LocateMe -h")
    assert_match "unknown option", shell_output("#{bin}/LocateMe -x 2>&1", 2)
  end
end
