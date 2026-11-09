class Autocannon < Formula
  desc "Fast HTTP/1.1 benchmarking tool written in Node.js"
  homepage "https://github.com/mcollina/autocannon"
  url "https://registry.npmjs.org/autocannon/-/autocannon-9.0.0.tgz"
  sha256 "a71969dd42cffd529f7dc6a0430b9aab830ba697e842360a2ee044f55b13672f"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "86c9c3693ee6c8c3b5299fe02f66039d5df8555a554d71033b5d15430d4b3c8c"
  end

  depends_on "node"

  allow_network_access! :build

  def install
    system "npm", "install", *std_npm_args
    bin.install_symlink Dir[libexec/"bin/*"]
  end

  test do
    output = shell_output("#{bin}/autocannon --connection 1 --duration 1 https://brew.sh 2>&1")
    assert_match "Running 1s test @ https://brew.sh", output

    assert_match version.to_s, shell_output("#{bin}/autocannon --version")
  end
end
