class Ccusage < Formula
  desc "CLI tool for analyzing Claude Code usage from local JSONL files"
  homepage "https://github.com/ccusage/ccusage"
  url "https://github.com/ccusage/ccusage/archive/refs/tags/v20.0.50.tar.gz"
  sha256 "07aa5fe8b7f53386b108fe7bba25f4610f52e391d6388ecafec3fcd43aa6c72c"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "07c5f70615233085770bca66c0b9f9e09b0337f0361ae48bbba9dd78c2f021f6"
  end

  depends_on "rust" => :build

  resource "litellm-pricing-json" do
    url "https://raw.githubusercontent.com/BerriAI/litellm/95ecfbbac60091dfb665e73c5f0eb7ef8ed899d7/model_prices_and_context_window.json"
    version "95ecfbbac60091dfb665e73c5f0eb7ef8ed899d7"
    sha256 "4d9f67ab0593c98c989b222d5290508e58299c01faa746517037dfa5b8794088"

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
