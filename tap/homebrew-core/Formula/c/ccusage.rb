class Ccusage < Formula
  desc "CLI tool for analyzing Claude Code usage from local JSONL files"
  homepage "https://github.com/ccusage/ccusage"
  url "https://github.com/ccusage/ccusage/archive/refs/tags/v20.0.49.tar.gz"
  sha256 "ac8a8f5b28e670820b43f9778b3a3c94d52a3e9a2f7dee22a407560f0e8cf04e"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "66212abc5eaf0a32b5ea0ac6df2e07925eb777d4c260068a4c8a3b87f386f5bd"
  end

  depends_on "rust" => :build

  resource "litellm-pricing-json" do
    url "https://raw.githubusercontent.com/BerriAI/litellm/263281ac89c9b81b17451f0c5c84126ccb2f6213/model_prices_and_context_window.json"
    version "263281ac89c9b81b17451f0c5c84126ccb2f6213"
    sha256 "2a9027b9816a876bb9f860741476eb5a240e8cd2f4252d6b528e4d604aef5c7b"

    # Fetch the latest available resource
    livecheck do
      url "https://api.github.com/repos/BerriAI/litellm/branches/main"
      strategy :json do |json|
        json.dig("commit", "sha")
      end
    end
  end

  deny_network_access!

  def fetch
    cd "rust" do
      system "cargo", "fetch", *std_cargo_fetch_args
    end
  end

  def install
    resource("litellm-pricing-json").stage buildpath
    ENV["CCUSAGE_PRICING_JSON_PATH"] = buildpath/"model_prices_and_context_window.json"
    system "cargo", "install", *std_cargo_args(path: "rust/crates/ccusage")
  end

  test do
    assert_match "No usage data found.", shell_output("#{bin}/ccusage 2>&1")
  end
end
