class Cx < Formula
  desc "CLI for querying and managing Coralogix observability data"
  homepage "https://github.com/coralogix/cx-cli"
  url "https://github.com/coralogix/cx-cli/archive/refs/tags/v0.1.34.tar.gz"
  sha256 "1fedcb9f723893c85222fbb2681e83c638b78a1d2999fb28258471b0987bfd1b"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d7b9e9e0dcda1b0fa521b3c878086e6e0921b040025e04282ec4bf1c8e78797c"
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
