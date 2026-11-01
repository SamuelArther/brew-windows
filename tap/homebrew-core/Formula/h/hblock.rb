class Hblock < Formula
  desc "Adblocker that creates a hosts file from multiple sources"
  homepage "https://hblock.molinero.dev/"
  url "https://github.com/hectorm/hblock/archive/refs/tags/v3.5.2.tar.gz"
  sha256 "dc59353e24edaf46ea0a9c746c8a6bbd4a468c354d6f64d4fafb70a32f990259"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "80330bae87ce593da13589c4725425ebdfe2fd3f4cf8eb4ef2730a09e4c5c874"
  end

  uses_from_macos "curl"

  def install
    system "make", "install", "prefix=#{prefix}", "bindir=#{bin}", "mandir=#{man}"
  end

  test do
    output = shell_output("#{bin}/hblock -H none -F none -S none -A none -D none -qO-")
    assert_match "Blocked domains:", output
  end
end
