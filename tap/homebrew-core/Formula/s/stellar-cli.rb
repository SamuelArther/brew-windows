class StellarCli < Formula
  desc "Stellar command-line tool for interacting with the Stellar network"
  homepage "https://developers.stellar.org"
  url "https://static.crates.io/crates/stellar-cli/stellar-cli-28.1.0.crate"
  sha256 "ceb241c122707ff7678e07c3459d43714eeae6af0c82c125f6a448e856805139"
  license "Apache-2.0"
  head "https://github.com/stellar/stellar-cli.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "6561fb4e3cd34fdf524478c699ec45be6b92514ae2064da85ca984084b4b63f9"
  end

  depends_on "rust" => :build

  on_linux do
    depends_on "pkgconf" => :build
    depends_on "dbus"
    depends_on "systemd" # for libudev
  end

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", "--bin=stellar", *std_cargo_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/stellar version")
    assert_match "TransactionEnvelope", shell_output("#{bin}/stellar xdr types list")
  end
end
