class Memtester < Formula
  desc "Utility for testing the memory subsystem"
  homepage "https://pyropus.ca/software/memtester/"
  url "https://pyropus.ca./software/memtester/old-versions/memtester-4.7.1.tar.gz", using: :homebrew_curl
  sha256 "e427de663f7bd22d1ebee8af12506a852c010bd4fcbca1e0e6b02972d298b5bb"
  license "GPL-2.0-only"

  # Despite the name, all the versions are seemingly found on this page. If this
  # doesn't end up being true over time, we can check the homepage instead.
  livecheck do
    url "https://pyropus.ca/software/memtester/old-versions/"
    regex(/href=.*?memtester[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8537671e514e4c57585dd1e54ec32a6651c49c85156a5394140e619d3652adfb"
  end

  def install
    inreplace "Makefile" do |s|
      s.change_make_var! "INSTALLPATH", prefix
      s.gsub! "man/man8", "share/man/man8"
    end
    inreplace "conf-ld", " -s", ""
    system "make", "install"
  end

  test do
    system bin/"memtester", "1", "1"
  end
end
