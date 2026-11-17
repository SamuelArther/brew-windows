class Drydock < Formula
  desc "Dashboard for a fleet of Git repositories"
  homepage "https://github.com/yetidevworks/drydock"
  url "https://github.com/yetidevworks/drydock/archive/refs/tags/v1.4.1.tar.gz"
  sha256 "36aac94ee78b552de65c1574d52f63271ad97c75ef9ba7fecff4db5d5481e90f"
  license "MIT"
  head "https://github.com/yetidevworks/drydock.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ffa45449340cbaa31efc526c45ddff2a8243a262671e718656b3d22351276527"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args(path: "crates/drydock")
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/drydock --version")

    output = shell_output("#{bin}/drydock config show")
    assert_match 'roots = ["~/Projects"]', output
  end
end
