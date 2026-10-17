class Mksh < Formula
  desc "MirBSD Korn Shell"
  homepage "https://mbsd.evolvis.org/mksh.htm"
  url "https://mbsd.evolvis.org/MirOS/dist/mir/mksh/mksh-R59c.tgz"
  version "59c"
  sha256 "77ae1665a337f1c48c61d6b961db3e52119b38e58884d1c89684af31f87bc506"
  license "MirOS"

  livecheck do
    url "https://mbsd.evolvis.org/MirOS/dist/mir/mksh/"
    regex(/href=.*?mksh-R?(\d+[a-z]?)\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "4300eec527899ec626ab41025be430dfa1af17371609e8d4e7581fb1d62f7c94"
  end

  def install
    system "sh", "./Build.sh", "-r"
    bin.install "mksh"
    man1.install "mksh.1"
  end

  test do
    assert_equal "honk",
      shell_output("#{bin}/mksh -c 'echo honk'").chomp
  end
end
