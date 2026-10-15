class Watchexec < Formula
  desc "Execute commands when watched files change"
  homepage "https://watchexec.github.io/"
  url "https://github.com/watchexec/watchexec/releases/download/v2.8.0/watchexec-2.8.0-x86_64-pc-windows-msvc.zip"
  sha256 "0c872fc18c8e801f21614c6daf2504a5826d5c705bb67f7a583d879f5410d9ea"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/watchexec.exe", "watchexec"
  end

  test do
    assert_match "watchexec #{version}", shell_output("#{bin}/watchexec --version")
  end
end
