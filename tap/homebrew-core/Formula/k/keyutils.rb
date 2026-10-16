class Keyutils < Formula
  desc "Key management utilities on Windows Credential Manager"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "keyctl", "keyctl"
  end

  def caveats
    <<~EOS
      keyctl keeps keys in Windows Credential Manager (generic credentials named keyctl/...).
      There is no libkeyutils: programs that need a key store on Windows use Credential Manager directly.
    EOS
  end

  test do
    assert_match "keyutils", shell_output("#{bin}/keyctl --version")
    assert_match "have_key_invalidate=1", shell_output("#{bin}/keyctl supports")
    shell_output("#{bin}/keyctl print 1 2>&1", 1)
  end
end
