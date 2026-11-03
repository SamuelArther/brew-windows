class StellarCli < Formula
  desc "Stellar command-line tool for interacting with the Stellar network"
  homepage "https://developers.stellar.org"
  url "https://static.crates.io/crates/stellar-cli/stellar-cli-30.0.1.crate"
  sha256 "c33c65392d9227c4628ecbb73041ae62bf81b25579badbabd27ebbbd6fa58992"
  license "Apache-2.0"
  head "https://github.com/stellar/stellar-cli.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e843301fad53ac1506b9ac714037ed88429ad712b940f0caf2f0d82f0d476b4d"
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
