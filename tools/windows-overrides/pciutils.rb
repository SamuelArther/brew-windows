class Pciutils < Formula
  desc "PCI utilities"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "lspci", "lspci"
    windows_exe "libexec/wintools.exe", "setpci", "setpci"
    windows_exe "libexec/wintools.exe", "update-pciids", "update-pciids"
  end

  test do
    assert_match "lspci version", shell_output("#{bin}/lspci --version")
    # Hyper-V CI machines may expose no PCI bus, so only require a clean run
    system bin/"lspci", "-nn"
    assert_match "ID registers", shell_output("#{bin}/setpci --help")
    assert_match "no device selected", shell_output("#{bin}/setpci VENDOR_ID 2>&1", 2)
  end
end
