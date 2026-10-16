class Katana < Formula
  desc "Crawling and spidering framework"
  homepage "https://github.com/projectdiscovery/katana"
  url "https://github.com/projectdiscovery/katana/archive/refs/tags/v1.8.0.tar.gz"
  sha256 "490f23c25daeea0ccf93268a81b4a89ba84ec43ac2bf6dc988392ec3a65d9329"
  license "MIT"
  head "https://github.com/projectdiscovery/katana.git", branch: "dev"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "92f7cc69993fb3f6f7678890470b6b6c3881a8ccf75d040f9ff6505dcb086c1a"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = "0"

    # Replace self-update with a notice; brew manages updates.
    inreplace "internal/runner/banner.go" do |s|
      s.gsub! 'updateutils "github.com/projectdiscovery/utils/update"',
              '_ "github.com/projectdiscovery/utils/update"'
      s.gsub! 'updateutils.GetUpdateToolCallback("katana", version)()',
              'gologger.Info().Msgf("Run `brew upgrade katana` to update.")'
    end

    ldflags = %W[-X github.com/projectdiscovery/katana/internal/runner.version=v#{version}]
    system "go", "build", *std_go_args(ldflags: ldflags), "./cmd/katana"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/katana -version 2>&1")
    assert_match "Started standard crawling", shell_output("#{bin}/katana -u 127.0.0.1 2>&1")
  end
end
