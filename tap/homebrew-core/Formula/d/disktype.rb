class Disktype < Formula
  desc "Detect content format of a disk or disk image"
  homepage "https://disktype.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/disktype/disktype/9/disktype-9.tar.gz"
  sha256 "b6701254d88412bc5d2db869037745f65f94b900b59184157d072f35832c1111"
  license "MIT"
  head "https://git.code.sf.net/p/disktype/disktype.git", branch: "master"

  livecheck do
    url :stable
    regex(%r{url=.*?/disktype[._-]v?(\d+(?:\.\d+)*)\.t}i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/disktype-9"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "ea05d2533443131b5d5cba948b2bc6eeed8a73a6fa1ff75e7d073df39b321694"
  end

  def install
    system "make"
    bin.install "disktype"
    man1.install "disktype.1"
  end

  test do
    path = testpath/"foo"
    path.write "1234"

    output = shell_output("#{bin}/disktype #{path}")
    assert_match "Regular file, size 4 bytes", output
  end
end
