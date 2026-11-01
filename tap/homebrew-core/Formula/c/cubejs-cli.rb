class CubejsCli < Formula
  desc "Cube.js command-line interface"
  homepage "https://cube.dev/"
  url "https://registry.npmjs.org/cubejs-cli/-/cubejs-cli-1.8.16.tgz"
  sha256 "b8e83f6c49d1e0265fb674ac036f21998e6044bc0bc397a88d4b178047545451"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "59f435f1e9683926513a4b35ac0261a25fd51b63f2e99747b7bf04a2b1ebf782"
  end

  depends_on "node"

  on_linux do
    depends_on "zlib-ng-compat"
  end

  deny_network_access!

  def fetch
    system "npm", "install", *std_npm_args(prefix: buildpath/"npm-fetch")
  end

  def install
    rm_r buildpath/"npm-fetch"
    system "npm", "install", "--offline", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    node_modules = libexec/"lib/node_modules/cubejs-cli/node_modules"
    deuniversalize_machos node_modules/"fsevents/fsevents.node" if OS.mac?
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cubejs --version")
    ENV["CI"] = "1" # Disable telemetry.
    token = "eyJhbGciOiJub25lIn0.eyJ1cmwiOiJodHRwczovL2V4YW1wbGUuY29tIn0."
    assert_match "Token successfully added!", shell_output("#{bin}/cubejs auth #{token}")
    config = JSON.parse((testpath/".cubecloud/config.json").read)
    assert_equal token, config.dig("auth", "https://example.com", "auth")
  end
end
