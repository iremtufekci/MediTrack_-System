
#ifndef ALGORITHM_H
#define ALGORITHM_H

#include <stdint.h>

void maxim_heart_rate_and_oxygen_saturation(uint32_t *ir_buffer, uint32_t *red_buffer, int32_t buffer_length,
                                            int32_t *spo2, int8_t *valid_spo2,
                                            int32_t *heart_rate, int8_t *valid_heart_rate);

#endif
