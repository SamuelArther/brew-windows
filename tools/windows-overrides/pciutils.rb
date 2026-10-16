class Pciutils < Formula
  desc "PCI utilities"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

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
