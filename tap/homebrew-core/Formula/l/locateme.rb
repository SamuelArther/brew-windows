class Locateme < Formula
  desc "Find your location using the Windows Location service"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

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
