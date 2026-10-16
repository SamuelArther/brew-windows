class M1ddc < Formula
  desc "Control external displays (luminance, contrast, volume, input) using DDC/CI"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

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
