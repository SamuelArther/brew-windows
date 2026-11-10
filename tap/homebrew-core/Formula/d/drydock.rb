class Drydock < Formula
  desc "Dashboard for a fleet of Git repositories"
  homepage "https://github.com/yetidevworks/drydock"
  url "https://github.com/yetidevworks/drydock/archive/refs/tags/v1.4.0.tar.gz"
  sha256 "b9f471e44de92982b7039f486d838aeb7d7f27baaae6b585ecd68fd3197ffca3"
  license "MIT"
  head "https://github.com/yetidevworks/drydock.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "dafd0e67df05cf47920f9dfdb7ed53b6488baf1d5833a83efd2eec10420968a3"
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
