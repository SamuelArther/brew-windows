class Batt < Formula
  desc "Show battery status and limit charging through ASUS and Dell laptop firmware"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.2/wintools-1.0.2.zip"
  sha256 "02a77df45114a8984fa643e351b9f8dfa5770746f08c6a808a594e486d0b5625"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "batt", "batt"
  end

  def caveats
    <<~EOS
      Charge limits are enforced by the laptop firmware (ASUS ATK WMI, Dell BIOS WMI) and need
      an elevated prompt to set. ASUS firmware forgets the limit on reboot; run
        batt install
      once (elevated) to re-apply it at every logon.
    EOS
  end

  test do
    assert_match "version v", shell_output("#{bin}/batt version")
    assert_match "Battery status:", shell_output("#{bin}/batt status")
    assert_match "must be between 10 and 100", shell_output("#{bin}/batt limit 5 2>&1", 2)
  end
end
