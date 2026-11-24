class Cx < Formula
  desc "CLI for querying and managing Coralogix observability data"
  homepage "https://github.com/coralogix/cx-cli"
  url "https://github.com/coralogix/cx-cli/archive/refs/tags/v0.1.36.tar.gz"
  sha256 "64a3852dcd452d95ad8a4b4ab8ed633bc7e983ac51aa6e02d93afafc13fbddde"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4d2527069b3df3ceb8a53bf07a67fe40b8e7cd88a722c123cac6e5b3f2129014"
  end

  depends_on "rust" => :build

  on_linux do
    depends_on "pkgconf" => :build
    depends_on "dbus"
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    ENV["CX_NO_UPDATE_NOTIFIER"] = "1"
    assert_match version.to_s, shell_output("#{bin}/cx --version")

    schema = JSON.parse(shell_output("#{bin}/cx schema"))
    assert_equal "cx", schema["name"]
  end
end
