class Mobiledevice < Formula
  desc "CLI to list connected iOS devices via the Apple Mobile Device service"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

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
