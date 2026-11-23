class Cx < Formula
  desc "CLI for querying and managing Coralogix observability data"
  homepage "https://github.com/coralogix/cx-cli"
  url "https://github.com/coralogix/cx-cli/archive/refs/tags/v0.1.35.tar.gz"
  sha256 "dc8febc230abf9aeaa8245f959dba5e7cf6f49125932d30751c3453ed531fe36"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "0cd9a150297f693ce69a10b3e183a76e1b7973eb9636c59afa0fdc8dd3206b65"
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
