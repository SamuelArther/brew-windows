class Dish < Formula
  desc "Lightweight monitoring service that efficiently checks socket connections"
  homepage "https://github.com/thevxn/dish"
  url "https://github.com/thevxn/dish/archive/refs/tags/v1.12.0.tar.gz"
  sha256 "4d8d18b837f03f7c3e03b6da79ffba1865256f2938f3995adafad48cb19a6b6f"
  license "MIT"
  head "https://github.com/thevxn/dish.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "a70819683e26db4c25ab67af8011222051547d5a58cad143d944582268ce9996"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/dish"
  end

  test do
    ouput = shell_output("#{bin}/dish https://example.com/:instance 2>&1", 3)
    assert_match "error loading socket list: failed to fetch sockets from remote source", ouput
  end
end
