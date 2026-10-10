import { Box, Container, Typography, Grid, Card, CardMedia, CardContent, Button } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { SAMPLE_MODELS } from './SampleModelsData';

const ENABLE_SAMPLES = false;

export function SampleModelsSection() {
  const navigate = useNavigate();
  if (!ENABLE_SAMPLES) return null;

  return (
    <Box sx={{ py: 8 }}>
      <Container maxWidth="lg">
        <Typography variant="h4" textAlign="center" fontWeight={700} mb={6}>Mô hình mẫu</Typography>
        <Grid container spacing={3}>
          {SAMPLE_MODELS.map((model) => (
            <Grid size={{ xs: 12, sm: 6, md: 3 }} key={model.id}>
              <Card variant="outlined" sx={{ bgcolor: 'background.paper' }}>
                <CardMedia component="img" height="140" image={model.imageUrl} alt={model.name} />
                <CardContent sx={{ textAlign: 'center' }}>
                  <Typography variant="subtitle1" fontWeight={600} mb={2}>{model.name}</Typography>
                  <Button variant="contained" size="small" onClick={() => navigate('/models')}>
                    Đặt in thử
                  </Button>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      </Container>
    </Box>
  );
}
