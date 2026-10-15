class DuaCli < Formula
  desc "View disk space usage and delete unwanted data, fast"
  homepage "https://lib.rs/crates/dua-cli"
  url "https://github.com/Byron/dua-cli/releases/download/v2.46.0/dua-v2.46.0-x86_64-pc-windows-msvc.zip"
  sha256 "49616d1deb36109274f19c09dd70420d45bd0c58ea1441236b65b2fd0bdc89ff"
  license "MIT"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    install_tree
    windows_exe "libexec/dua.exe", "dua"
  end

  test do
    assert_match "dua #{version}", shell_output("#{bin}/dua --version")
  end
end
