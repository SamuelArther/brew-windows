class DuaCli < Formula
  desc "View disk space usage and delete unwanted data, fast"
  homepage "https://lib.rs/crates/dua-cli"
  version "2.45.1"
  url "https://github.com/Byron/dua-cli/releases/download/v2.45.1/dua-v2.45.1-x86_64-pc-windows-msvc.zip"
  sha256 "48c245f987c5359df0302202e6338e3235e4a4724db58d7d66138b096e8b2015"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/dua.exe", "dua"
  end

  test do
    assert_match "dua #{version}", shell_output("#{bin}/dua --version")
  end
end
