class Hey < Formula
  desc "HTTP load generator, ApacheBench (ab) replacement"
  homepage "https://github.com/rakyll/hey"
  url "https://github.com/rakyll/hey/archive/refs/tags/v0.1.5.tar.gz"
  sha256 "f678bc0f07c62a6513726298873940b70099aa85244efa813f6a0d925092ffe9"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "466a367c5d3710b09f541074d9f75678ffc4bacc3a695399da6adbb006d4fd64"
  end

  depends_on "go" => :build

  # `test do` block load-tests example.com
  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    output = "[200]	200 responses"
    assert_match output.to_s, shell_output("#{bin}/hey https://example.com")
  end
end
