class Cx < Formula
  desc "CLI for querying and managing Coralogix observability data"
  homepage "https://github.com/coralogix/cx-cli"
  url "https://github.com/coralogix/cx-cli/archive/refs/tags/v0.1.33.tar.gz"
  sha256 "4b4e88c2208806d545eccb5a5abc72b1ca9e3f0f585613c984fe23c4d7d6b212"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "561834c3b080625a96c50c67fb0c5f752264eec2e3174372c878ef527aef0746"
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
