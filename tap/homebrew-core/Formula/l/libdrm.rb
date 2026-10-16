class Libdrm < Formula
  desc "GPU and display mode-setting tools (libdrm drmdevice, modetest, modeprint)"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "drmdevice", "drmdevice"
    windows_exe "libexec/wintools.exe", "modetest", "modetest"
    windows_exe "libexec/wintools.exe", "modeprint", "modeprint"
  end

  test do
    # GitHub's runners have a (virtual) display adapter; the listing works with any number of GPUs.
    assert_match "--- Checking the number of DRM device available ---", shell_output("#{bin}/drmdevice")
    assert_match "Connectors:", shell_output("#{bin}/modetest -c")
    assert_match "CRTCs:", shell_output("#{bin}/modetest -p")
    assert_match "count_connectors", shell_output("#{bin}/modeprint")
    assert_match "failed to find connector", shell_output("#{bin}/modetest -s 9999:640x480 2>&1", 1)
  end
end
