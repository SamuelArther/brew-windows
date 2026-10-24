class PlzCli < Formula
  desc "Copilot for your terminal"
  homepage "https://github.com/m1guelpf/plz-cli"
  url "https://github.com/m1guelpf/plz-cli/archive/refs/tags/v0.1.9.tar.gz"
  sha256 "ea6b65deb69f86e53210d2457f49a53bb73ab52282fccc5b6cae8e8c059ecb00"
  license "MIT"
  head "https://github.com/m1guelpf/plz-cli.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f2f33a6b1e26b785cabad2cb17b3a6ed34c5ba9b575c19635bb17dcd5c418781"
  end

  depends_on "rust" => :build

  allow_network_access! :test

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    ENV["OPENAI_API_KEY"] = "sk-XXXXXXXX"
    assert_match "API error", shell_output("#{bin}/plz brewtest", 1)
  end
end
