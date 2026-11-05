class Cx < Formula
  desc "CLI for querying and managing Coralogix observability data"
  homepage "https://github.com/coralogix/cx-cli"
  url "https://github.com/coralogix/cx-cli/archive/refs/tags/v0.1.31.tar.gz"
  sha256 "f217ae62ba6eb7703f5f69fce06f774b2ccf5d46f6eed0240f6f978f49257dd6"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "adc29da314deb922fb9a3014d1035fae9c4065e2821efb2fe3f887c8a82c3837"
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
