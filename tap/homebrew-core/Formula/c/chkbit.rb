class Chkbit < Formula
  desc "Check your files for data corruption"
  homepage "https://laktak.github.io/chkbit/"
  url "https://github.com/laktak/chkbit/releases/download/v6.6.0/chkbit-windows-amd64.zip"
  sha256 "cf2922856c2499a9ea98775529a3bd25ca0c7e2fc41b8bb0f6f2c90f11fcc058"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/laktak/chkbit/archive/refs/tags/v6.6.0.tar.gz"
  end

  def install
    bin.install "chkbit.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/chkbit version").chomp
    system bin/"chkbit", "init", "split", testpath
    assert_path_exists testpath/".chkbit"
  end
end
