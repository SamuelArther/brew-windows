class Yarn < Formula
  desc "JavaScript package manager"
  homepage "https://classic.yarnpkg.com/"
  url "https://github.com/yarnpkg/yarn/releases/download/v1.22.22/yarn-v1.22.22.tar.gz"
  sha256 "88268464199d1611fcf73ce9c0a6c4d44c7d5363682720d8506f6508addf36a0"
  license "BSD-2-Clause"

  livecheck do
    skip("1.x line is frozen and features/bugfixes only happen on 2+")
  end

  depends_on "node"

  # Yarn Classic, matching the standalone Homebrew yarn package.

  def install
    install_tree
    node_script "libexec/bin/yarn.js", "yarn"
    node_script "libexec/bin/yarn.js", "yarnpkg"
  end

  test do
    (testpath/"package.json").write('{"name": "test"}')
    system bin/"yarn", "add", "jquery"
    assert_path_exists testpath/"node_modules/jquery/package.json"
  end
end
