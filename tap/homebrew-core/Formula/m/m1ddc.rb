class M1ddc < Formula
  desc "Control external displays (luminance, contrast, volume, input) using DDC/CI"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "m1ddc", "m1ddc"
  end

  test do
    assert_includes shell_output("#{bin}/m1ddc help", 1), "Controls volume, luminance"
    # CI has no DDC/CI monitor: either none is found or it doesn't answer
    assert_match(/Could not find a suitable external display\.|DDC communication failure/,
                 shell_output("#{bin}/m1ddc get luminance 2>&1", 1))
  end
end
