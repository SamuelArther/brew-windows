class OnedriveCli < Formula
  desc "Folder synchronization with OneDrive via the built-in client"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.1/wintools-1.0.1.zip"
  sha256 "04cfc5f238f0900874f633abcb7f0ccbdb70db438b3bab6a8816082868d2661c"

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
