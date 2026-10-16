class Drill < Formula
  desc "HTTP load testing application written in Rust"
  homepage "https://github.com/fcsonline/drill"
  url "https://github.com/fcsonline/drill/archive/refs/tags/0.9.1.tar.gz"
  sha256 "79e90ba78e484e15bf6cc919d0be5fdd99155ff2d5b16581539e4361191a8bc7"
  license "GPL-3.0-or-later"
  head "https://github.com/fcsonline/drill.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "cde152bdde9d989055e08cab2a8133ab20446595adecb9f04de016e9a5fc37b0"
  end

  depends_on "pkgconf" => :build
  depends_on "rust" => :build

  conflicts_with "ldns", because: "both install a `drill` binary"

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    (testpath/"benchmark.yml").write <<~YAML
      ---
      concurrency: 4
      base: 'https://dummyjson.com'
      iterations: 5
      rampup: 2

      plan:
        - name: Http status
          request:
            url: /http/200

        - name: Check products API
          request:
            url: /products/1
    YAML

    assert_match "Total requests            10",
      shell_output("#{bin}/drill --benchmark #{testpath}/benchmark.yml --stats")
  end
end
