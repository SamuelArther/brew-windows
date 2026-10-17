class Cryptopp < Formula
  desc "Free C++ class library of cryptographic schemes"
  homepage "https://github.com/weidai11/cryptopp"
  url "https://github.com/weidai11/cryptopp/releases/download/CRYPTOPP_8_9_0/cryptopp890.zip"
  version "8.9.0"
  sha256 "4cc0ccc324625b80b695fcd3dee63a66f1a460d3e51b71640cdbfc4cd1a3779c"
  license all_of: [:public_domain, "BSL-1.0"]
  head "https://github.com/weidai11/cryptopp.git", branch: "master"

  livecheck do
    url :head
    regex(/^CRYPTOPP[._-]V?(\d+(?:[._-]\d+)+)$/i)
    strategy :git do |tags, regex|
      tags.map { |tag| tag[regex, 1]&.tr("_", ".") }
    end
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/cryptopp-8.9.0"
    sha256 cellar: :any, x86_64_windows: "cae1b34cf0de51575247ad2f541264553a546239c1fcc70c128201eeee80066b"
  end

  deny_network_access!

  def install
    ENV.runtime_cpu_detection # https://github.com/weidai11/cryptopp/blob/master/cpu.h

    system "make", "all", "libcryptopp.pc", "PREFIX=#{prefix}"
    system "make", "test"
    system "make", "install-lib", "PREFIX=#{prefix}"
  end

  test do
    # Test program modified from:
    #   https://www.cryptopp.com/wiki/Advanced_Encryption_Standard
    (testpath/"test.cc").write <<~CPP
      #ifdef NDEBUG
      #undef NDEBUG
      #endif
      #include <cassert>
      #include <iostream>
      #include <string>

      #include <cryptopp/cryptlib.h>
      #include <cryptopp/modes.h>
      #include <cryptopp/osrng.h>
      #include <cryptopp/rijndael.h>

      int main(int argc, char *argv[]) {
        using namespace CryptoPP;

        AutoSeededRandomPool prng;

        SecByteBlock key(AES::DEFAULT_KEYLENGTH);
        SecByteBlock iv(AES::BLOCKSIZE);

        prng.GenerateBlock(key, key.size());
        prng.GenerateBlock(iv, iv.size());

        std::string plain = "Hello, Homebrew!";
        std::string cipher;
        std::string recovered;

        try {
          CBC_Mode<AES>::Encryption e;
          e.SetKeyWithIV(key, key.size(), iv);
          StringSource s(plain, true,
              new StreamTransformationFilter(e, new StringSink(cipher)));
        } catch (const Exception &e) {
          std::cerr << e.what() << std::endl;
          exit(1);
        }

        try {
          CBC_Mode<AES>::Decryption d;
          d.SetKeyWithIV(key, key.size(), iv);
          StringSource s(cipher, true,
              new StreamTransformationFilter(d, new StringSink(recovered)));
        } catch (const Exception &e) {
          std::cerr << e.what() << std::endl;
          exit(1);
        }

        assert(plain == recovered);
        return 0;
      }
    CPP
    system ENV.cxx, "test.cc", "-I#{include}", "-L#{lib}",
                    "-lcryptopp", "-o", "test"
    system "./test"
  end
end
