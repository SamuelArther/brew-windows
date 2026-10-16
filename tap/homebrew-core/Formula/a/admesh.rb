class Admesh < Formula
  desc "Processes triangulated solid meshes"
  homepage "https://github.com/admesh/admesh"
  url "https://github.com/admesh/admesh/releases/download/v0.98.5/admesh-0.98.5.tar.gz"
  sha256 "0d7994bfa587c4e958b2ac7c7d2fb90dfb6c5463d32513ada169cf710a438535"
  license "GPL-2.0-or-later"

  deny_network_access!

  bottle do
    sha256 cellar: :any, x86_64_windows: "8c4be88c4e0fc190fe66fffc622275c49beb0b781ce12be3e20252a428ce5b7c"
  end

  def install
    system "./configure", "--disable-dependency-tracking",
                          "--disable-silent-rules",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    # Test file is the beginning of block.stl from admesh's source
    (testpath/"test.stl").write <<~STL
      SOLID Untitled1
      FACET NORMAL  0.00000000E+00  0.00000000E+00  1.00000000E+00
      OUTER LOOP
      VERTEX -1.96850394E+00  1.96850394E+00  1.96850394E+00
      VERTEX -1.96850394E+00 -1.96850394E+00  1.96850394E+00
      VERTEX  1.96850394E+00 -1.96850394E+00  1.96850394E+00
      ENDLOOP
      ENDFACET
      ENDSOLID Untitled1
    STL
    system bin/"admesh", "test.stl"
  end
end
