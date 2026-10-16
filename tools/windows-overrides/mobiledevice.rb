class Mobiledevice < Formula
  desc "CLI to list connected iOS devices via the Apple Mobile Device service"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "mobiledevice", "mobiledevice"
  end

  test do
    system bin/"mobiledevice", "list_devices"
    assert_match(/\A\S*\s?v?\d+(\.\d+)+/, shell_output("#{bin}/mobiledevice version").strip)
    assert_match "list_devices", shell_output("#{bin}/mobiledevice help")
  end
end
