class Gel < Formula
  desc "Modern gem manager"
  homepage "https://gel.dev"
  url "https://github.com/gel-rb/gel/archive/refs/tags/v0.3.0.tar.gz"
  sha256 "fe7c4bd67a2ea857b85b754f5b4d336e26640eda7199bc99b9a1570043362551"
  license "MIT"
  revision 1
  head "https://github.com/gel-rb/gel.git", branch: "main"

  # https://github.com/gel-rb/gel/issues/134
  deprecate! date: "2024-12-25", because: :unmaintained
  disable! date: "2025-12-25", because: :unmaintained

  depends_on "ronn" => :build

  uses_from_macos "ruby"

  def install
    system "rake", "man"
    bin.install "exe/gel"
    prefix.install "lib"
    man1.install Pathname.glob("man/man1/*.1")
  end

  test do
    (testpath/"Gemfile").write <<~RUBY
      source "https://rubygems.org"
      gem "gel"
    RUBY
    system bin/"gel", "install"
  end
end
