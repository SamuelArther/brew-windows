class OnedriveCli < Formula
  desc "Folder synchronization with OneDrive via the built-in client"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.0/wintools-1.0.0.zip"
  sha256 "c498adc5a15d92a66624742f12671249774e8c655160dba58c4c18b6bbef537b"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "onedrive", "onedrive"
  end

  test do
    output = pipe_output("#{bin}/onedrive 2>&1", "")
    assert_match "Attempting to contact the Microsoft OneDrive Service", output
    assert_match "Successfully reached the Microsoft OneDrive Service", output
    assert_match "onedrive v", shell_output("#{bin}/onedrive --version")
  end
end
