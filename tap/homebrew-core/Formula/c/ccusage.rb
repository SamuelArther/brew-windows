class Ccusage < Formula
  desc "CLI tool for analyzing Claude Code usage from local JSONL files"
  homepage "https://github.com/ccusage/ccusage"
  url "https://github.com/ccusage/ccusage/archive/refs/tags/v20.0.36.tar.gz"
  sha256 "fbf05898640a745f257078f2b067a6a1da0fd0c872599ef6c921b4e89b0a8198"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f10f0f51095da7ee53129ef1bb4d1a2b6b579b1ac665964bd44324e87b9ab887"
  end

  depends_on "rust" => :build

  resource "litellm-pricing-json" do
    url "https://raw.githubusercontent.com/BerriAI/litellm/9c958aff349680503a5d9785b3ef507eb9822002/model_prices_and_context_window.json"
    version "9c958aff349680503a5d9785b3ef507eb9822002"
    sha256 "da798353b3d14c09a13f2f1fe42eaf903212c158bb6d94d724836225c68d865d"

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
