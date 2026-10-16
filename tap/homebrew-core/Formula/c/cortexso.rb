class Cortexso < Formula
  desc "Drop-in, local AI alternative to the OpenAI stack"
  homepage "https://jan.ai/cortex"
  url "https://registry.npmjs.org/cortexso/-/cortexso-0.1.1.tgz"
  sha256 "48efc16761eebfdd60e50211049554e7b781b30e56461042c6bf100e84d8d244"
  license "Apache-2.0"
  head "https://github.com/janhq/cortex.git", branch: "dev"

  deprecate! date: "2025-07-19", because: :repo_archived
  disable! date: "2026-07-19", because: :repo_archived

  depends_on "node"
  depends_on "sqlite" # needs sqlite3_enable_load_extension

  on_linux do
    # Workaround for old `node-gyp` that needs distutils.
    # TODO: Remove when `node-gyp` is v10+
    depends_on "python-setuptools" => :build
  end

  conflicts_with "cortex", because: "both install `cortex` binaries"

  def install
    system "npm", "install", "--sqlite=#{formula_opt_prefix("sqlite")}", *std_npm_args
    bin.install_symlink libexec.glob("bin/*")

    # Replace pre-built binaries
    rm_r(libexec/"lib/node_modules/cortexso/node_modules/cpu-instructions/prebuilds")
    cd libexec/"lib/node_modules/cortexso/node_modules/cpu-instructions" do
      system "npm", "run", "build"
      Pathname.glob("prebuilds/*/cpu-instructions.node").map { |f| f.rename(f.dirname/"cpuinfo.node") }
    end
  end

  test do
    port = free_port
    pid = fork { exec bin/"cortex", "serve", "--port", port.to_s }
    sleep 10
    begin
      assert_match "OK", shell_output("curl -s localhost:#{port}/v1/health")
    ensure
      Process.kill "SIGTERM", pid
    end
  end
end
