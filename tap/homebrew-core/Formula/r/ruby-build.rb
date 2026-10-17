class RubyBuild < Formula
  desc "Install various Ruby versions and implementations"
  homepage "https://rbenv.org/man/ruby-build.1"
  url "https://github.com/rbenv/ruby-build/archive/refs/tags/v20261013.tar.gz"
  sha256 "9e683b1d8efe337ae27c31f995641e22761af603bb0f783d22c5d2f99252758c"
  license "MIT"
  compatibility_version 1
  head "https://github.com/rbenv/ruby-build.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d7b6d58dc6e26f67c8a231eddf4b4e5c186d8e8729dd6da580d445b50fcbf60a"
  end

  depends_on "autoconf"
  depends_on "libyaml"
  depends_on "openssl@3" # TODO: OpenSSL 4 needs https://github.com/rbenv/ruby-build/pull/2647
  depends_on "pkgconf"
  depends_on "readline"

  def install
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    assert_match "2.0.0", shell_output("#{bin}/ruby-build --definitions")
  end
end
