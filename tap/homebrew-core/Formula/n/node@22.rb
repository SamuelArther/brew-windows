class NodeAT22 < Formula
  desc "Open-source, cross-platform JavaScript runtime environment"
  homepage "https://nodejs.org/"
  version "22.23.3"
  url "https://nodejs.org/dist/v22.23.3/node-v22.23.3-win-x64.zip"
  sha256 "2b0ff57b049cda1bbcea2240eec20467018713c1efe1f7360c2681859b90ed71"
  license "MIT"

  livecheck do
    url "https://nodejs.org/dist/"
    regex(%r{href=["']?v?(22(?:\.\d+)+)/?["' >]}i)
  end

  keg_only :versioned_formula

  # https://github.com/nodejs/release#release-schedule
  deprecate! date: "2026-10-28", because: :unsupported

  def install
    install_tree
    windows_exe "libexec/node.exe", "node"
    windows_cmd "libexec/npm.cmd", "npm"
    windows_cmd "libexec/npx.cmd", "npx"
  end

  test do
    path = testpath/"test.js"
    path.write "console.log('hello');"

    output = shell_output("#{bin}/node #{path}").strip
    assert_equal "hello", output
    assert_match version.to_s, shell_output("#{bin}/node --version")
  end
end
