class Libjuice < Formula
  desc "UDP Interactive Connectivity Establishment (ICE) library"
  homepage "https://github.com/paullouisageneau/libjuice"
  url "https://github.com/paullouisageneau/libjuice/archive/refs/tags/v1.7.4.tar.gz"
  sha256 "95c088862aa1b88b73d62aa99577135010eb768fcafb252d13aa85a8515ef798"
  license "MPL-2.0"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/libjuice-1.7.4"
    sha256 cellar: :any, x86_64_windows: "2c1972a2c8ad598c282fe071b94eeb4cae339fe725e4e2dc3370a7f03e6d19f7"
  end

  depends_on "cmake" => :build

  deny_network_access!
  def install
    system "cmake", "-S", ".", "-B", "build", "-DNO_TESTS=1", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h>
      #include <string.h>
      #include "juice/juice.h"

      int main() {
          juice_config_t config;
          memset(&config, 0, sizeof(config));

          config.stun_server_host = "stun.l.google.com";
          config.stun_server_port = 19302;
          config.turn_servers = NULL;
          config.turn_servers_count = 0;
          config.user_ptr = NULL;
          config.cb_state_changed = NULL;
          config.cb_candidate = NULL;
          config.cb_gathering_done = NULL;
          config.cb_recv = NULL;

          juice_agent_t *agent = juice_create(&config);
          if (agent == NULL)
              return 1;
          printf("Successfully created a juice agent\\n");

          juice_destroy(agent);
          printf("Successfully destroyed the juice agent\\n");

          return 0;
      }
    C

    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-ljuice", "-o", "test"
    system "./test"
  end
end
