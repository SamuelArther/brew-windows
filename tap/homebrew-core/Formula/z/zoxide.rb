class Zoxide < Formula
  desc "Shell extension to navigate your filesystem faster"
  homepage "https://github.com/ajeetdsouza/zoxide"
  version "0.10.0"
  url "https://github.com/ajeetdsouza/zoxide/releases/download/v0.10.0/zoxide-0.10.0-x86_64-pc-windows-msvc.zip"
  sha256 "f465ae548f8754c8e7edbc60b45fbf58c92bfe123db83d790252d6810fa5daf1"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/zoxide.exe", "zoxide"
  end

  test do
    assert_match "zoxide #{version}", shell_output("#{bin}/zoxide --version")
  end
end
